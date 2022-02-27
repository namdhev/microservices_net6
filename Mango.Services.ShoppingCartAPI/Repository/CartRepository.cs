using System.Linq;
using AutoMapper;
using Mango.Services.ShoppingCartAPI.DbContexts;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Microsoft.EntityFrameworkCore;

#nullable disable
namespace Mango.Services.ShoppingCartAPI.Repository
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _db;
        private  IMapper _mapper;

        public CartRepository(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<CartDto> GetCartByUserId(string userId)
        {
            try
            {
                Cart cart = new()
                {
                    CartHeader = await _db.CartHeaders.FirstOrDefaultAsync(cartHeader => cartHeader.UserId == userId)
                };

                cart.CartDetails =
                    _db.CartDetails.Where(cartDetail => cartDetail.CartHeaderId == cart.CartHeader.CartHeaderId)
                        .Include(cd => cd.Product);

                return _mapper.Map<CartDto>(cart);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occurred while fetching cart by user id\n", e.Message);
                return null;
            }

        }

        public async Task<CartDto> CreateUpdateCart(CartDto cartDto)
        {
            Cart cart = _mapper.Map<Cart>(cartDto);
            // Check and add if product does not exist in DB
            var prodInDb = await _db.Products
                .FirstOrDefaultAsync(pdt => pdt.ProductId == cartDto.CartDetails.FirstOrDefault()
                .ProductId);
            if (prodInDb == null)
            {
                _db.Products.Add(cart.CartDetails.FirstOrDefault().Product);
                await _db.SaveChangesAsync();
            }   


            // Check and add if cart header not found
            var cartHeaderFromDb = await _db.CartHeaders.AsNoTracking()
                .FirstOrDefaultAsync(ch => ch.UserId == cart.CartHeader.UserId);
            if (cartHeaderFromDb == null)
            {
                _db.CartHeaders.Add(cart.CartHeader);
                await _db.SaveChangesAsync();

                // Add cart details
                cart.CartDetails.FirstOrDefault().CartHeaderId = cart.CartHeader.CartHeaderId;
                cart.CartDetails.FirstOrDefault().Product = null; // to avoid conflict of inserting existing product id
                _db.CartDetails.Add(cart.CartDetails.FirstOrDefault());
                await _db.SaveChangesAsync();
            }
            // update if cart header is not null
            else
            {
                var cartDetailsFromDb = await _db.CartDetails.AsNoTracking().FirstOrDefaultAsync(cd =>
                    cd.ProductId == cart.CartDetails.FirstOrDefault().ProductId
                    &&
                    cd.CartHeaderId == cartHeaderFromDb.CartHeaderId);
                // check if cart detail belongs to same product
                if (cartDetailsFromDb == null)
                {
                    // add if cart detail does not belong to same product
                    cart.CartDetails.FirstOrDefault().CartHeaderId = cartHeaderFromDb.CartHeaderId;
                    cart.CartDetails.FirstOrDefault().Product = null; // to avoid conflict of inserting existing product id
                    _db.CartDetails.Add(cart.CartDetails.FirstOrDefault());
                    await _db.SaveChangesAsync();
                }
                else
                {
                    // update count if cart detail belongs to same product
                    cart.CartDetails.FirstOrDefault().Product = null; // to avoid conflict of inserting existing product id
                    cart.CartDetails.FirstOrDefault().Count += cartDetailsFromDb.Count;
                    cart.CartDetails.FirstOrDefault().CartHeaderId = cartHeaderFromDb.CartHeaderId;
                    cart.CartDetails.FirstOrDefault().CartDetailId = cartDetailsFromDb.CartDetailId;
                    _db.CartDetails.Update(cart.CartDetails.FirstOrDefault());
                    await _db.SaveChangesAsync();
                }
            }

            return _mapper.Map<CartDto>(cart);
        }

        public async Task<bool> RemoveFromCart(int cartDetailsId)
        {
            try
            {
                var cartDetails =
                    await _db.CartDetails.FirstOrDefaultAsync(cartDetail => cartDetail.CartDetailId == cartDetailsId);

                int totalCartItems =
                    _db.CartDetails.Where(cartDetail => cartDetail.CartHeaderId == cartDetails.CartHeaderId).Count();

                _db.CartDetails.Remove(cartDetails);
                if (totalCartItems == 1)
                {
                    var cartHeaderToRemove =
                        await _db.CartHeaders.FirstOrDefaultAsync(cheader =>
                            cheader.CartHeaderId == cartDetails.CartHeaderId);
                    _db.CartHeaders.Remove(cartHeaderToRemove);
                }

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occurred while removing cart items\n", e.Message);
                return false;
            }
        }

        public async Task<bool> ApplyCoupon(string userId, string couponCode)
        {
            try
            {
                var cartFromDb = await _db.CartHeaders.FirstOrDefaultAsync(cartHeader => cartHeader.UserId == userId);
                cartFromDb.CouponCode = couponCode;
                _db.CartHeaders.Update(cartFromDb);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

        }

        public async Task<bool> RemoveCoupon(string userId)
        {
            try
            {
                var cartFromDb = await _db.CartHeaders.FirstOrDefaultAsync(cartHeader => cartHeader.UserId == userId);
                cartFromDb.CouponCode = "";
                _db.CartHeaders.Update(cartFromDb);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task<bool> ClearCart(string userId)
        {
            try
            {
                var cartHeaderFromDb = await _db.CartHeaders.FirstOrDefaultAsync(cartHeader => cartHeader.UserId == userId);

                if (cartHeaderFromDb != null)
                {
                    _db.CartDetails.RemoveRange(_db.CartDetails
                        .Where(cartDetail => cartDetail.CartHeaderId == cartHeaderFromDb.CartHeaderId));
                    _db.CartHeaders.Remove(cartHeaderFromDb);
                    await _db.SaveChangesAsync();
                    return true;
                }

                return false;

            }
            catch (Exception e)
            {
                Console.WriteLine("Error occurred while clearing cart\n", e.Message);
                return false;
            }
        }
    }
}

